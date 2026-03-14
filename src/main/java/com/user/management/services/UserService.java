package com.user.management.services;

import java.util.List;

import org.modelmapper.ModelMapper;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;
import com.user.management.exceptions.UserNotFoundException;
import com.user.management.dtos.UserResponseDTO;
import com.user.management.repository.UserRepository;
import com.user.management.models.User;

@Service
public class UserService {

    @Autowired
    private ModelMapper modelMapper;

    @Autowired
    private UserRepository userRepository;

    public UserResponseDTO saveUser(User user) {
        
        if (user.getName() == null || user.getName().isBlank() || 
        user.getLogin() == null || user.getLogin().isBlank() ||
        user.getPassword() == null || user.getPassword().isBlank()) {
            //return ResponseEntity.status(400).body("Don't Find the name or login");
            throw new IllegalArgumentException("Dont Find the name or login or password");
        }
        
    
        User saveUser = userRepository.save(user);
        return modelMapper.map(saveUser, UserResponseDTO.class);
    }

    public List<UserResponseDTO> getAllUsers() {
        return userRepository.findAll()
                .stream()
                .map(user -> modelMapper.map(user, UserResponseDTO.class))
                .toList();
    }
    
    public UserResponseDTO getUserById(String id) {
        User user = userRepository.findById(id)
                .orElseThrow(() -> new UserNotFoundException("Usuario con id " + id + " no encontrado"));
        return modelMapper.map(user, UserResponseDTO.class);
    }

}
