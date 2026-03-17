package com.user.management.dtos;

import jakarta.validation.constraints.NotBlank;
import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
public class UserRequestDTO {
    @NotBlank(message = "El nombre es obligatorio")
    private String name;

    @NotBlank(message = "El login es obligatorio")
    private String login;

    @NotBlank(message = "La contraseña es obligatoria")
    private String password;

}
